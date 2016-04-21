package deleteFiles;
import java.io.File;
import java.io.IOException;
 
/**
 * @author Crunchify.com
 *
 */
 
public class CrunchifyDeleteFiles {
	
	public CrunchifyDeleteFiles(){}
 
	public static void crunchifyDeleteFiles(File myFile) throws IOException {
		if (myFile.isDirectory()) {
			// Returns an array of strings naming the files and directories in the directory denoted by this abstract
			// pathname.
			String crunchifyFiles[] = myFile.list();
			for (String file : crunchifyFiles) {
				// Creates a new File instance from a parent abstract pathname and a child pathname string.
				File fileDelete = new File(myFile, file);
 
				// recursion
				crunchifyDeleteFiles(fileDelete);
			}
		} 
		else {
			// Deletes the file or directory denoted by this abstract pathname. If this pathname denotes a directory,
			// then the directory must be empty in order to be deleted.
			myFile.delete();
			System.out.println("File is deleted : " + myFile.getAbsolutePath());
		}
	}
	
	public static void deleteThisFile(File directory) throws IOException{
  		if (!directory.exists()) {
   			System.out.println("File does not exist: " + directory);
   		}
   		else{
   			crunchifyDeleteFiles(directory);
   		}
	}
}	
 
